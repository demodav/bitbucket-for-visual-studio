﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using BitBucket.REST.API.Models.Standard;
using GitClientVS.Contracts.Interfaces;
using GitClientVS.Contracts.Interfaces.Services;
using GitClientVS.Contracts.Interfaces.ViewModels;
using GitClientVS.Contracts.Models.GitClientModels;
using GitClientVS.Contracts.Models.Tree;
using ReactiveUI;

namespace GitClientVS.Infrastructure.ViewModels
{
    [Export(typeof(ICommentViewModel))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class CommentViewModel : ViewModelBase, ICommentViewModel
    {
        private List<ICommentTree> _commentTree;
        private List<ICommentTree> _inlineCommentTree;
        private int _commentsCount;
        private readonly IGitClientService _gitClientService;
        private readonly ITreeStructureGenerator _treeStructureGenerator;
        private string _addCommentText;

        public ReactiveCommand ReplyCommentCommand { get; private set; }
        public ReactiveCommand EditCommentCommand { get; private set; }
        public ReactiveCommand DeleteCommentCommand { get; private set; }
        public ReactiveCommand AddCommentCommand { get; private set; }

        public long PullRequestId { get; private set; }

        public List<ICommentTree> CommentTree
        {
            get => _commentTree;
            private set => this.RaiseAndSetIfChanged(ref _commentTree, value);
        }

        public int CommentsCount
        {
            get => _commentsCount;
            private set => this.RaiseAndSetIfChanged(ref _commentsCount, value);
        }

        public string AddCommentText
        {
            get => _addCommentText;
            set => this.RaiseAndSetIfChanged(ref _addCommentText, value);
        }

        public List<ICommentTree> InlineCommentTree
        {
            get => _inlineCommentTree;
            private set => this.RaiseAndSetIfChanged(ref _inlineCommentTree, value);
        }

        [ImportingConstructor]
        public CommentViewModel(IGitClientService gitClientService, ITreeStructureGenerator treeStructureGenerator)
        {
            _gitClientService = gitClientService;
            _treeStructureGenerator = treeStructureGenerator;
        }

        public async Task UpdateComments(long pullRequestId)
        {
            PullRequestId = pullRequestId;
            var comments = await _gitClientService.GetPullRequestComments(PullRequestId);

            var inlineComments = comments.Where(comment => comment.IsInline).ToList();
            var notInlineComments = comments.Where(x => !x.IsInline).ToList();

            InlineCommentTree = _treeStructureGenerator.CreateCommentTree(inlineComments).ToList();
            CommentTree = _treeStructureGenerator.CreateCommentTree(notInlineComments).ToList();
            CommentsCount = notInlineComments.Count(x => !x.IsDeleted);
        }

        private async Task ReplyToComment(ICommentTree commentTree)
        {
            var comment = commentTree.Comment;

            var newComment = new GitComment()
            {
                Content = comment.Content,
                Parent = new GitCommentParent() { Id = comment.Id },
                Inline = comment.Inline !=null ? new GitCommentInline() {Path = comment.Inline.Path} : null
            };

            await _gitClientService.AddPullRequestComment(PullRequestId, newComment);
            await UpdateComments(PullRequestId);//todo temp solution just to make it work
        }

        private Task EditComment(ICommentTree commentTree)
        {
            throw new NotImplementedException();
        }

        private async Task AddComment(GitCommentInline inline)
        {
            var comment = new GitComment()
            {
                Content = new GitCommentContent() { Html = AddCommentText },
                IsInline = inline != null,
                Inline = inline != null ? new GitCommentInline()
                {

                    Path = inline.Path,
                    From = inline.From,
                    To = inline.To
                } : null
            };

            await _gitClientService.AddPullRequestComment(PullRequestId, comment);
            await UpdateComments(PullRequestId); //todo temp solution just to make it work
            AddCommentText = string.Empty;
        }

        private async Task DeleteComment(ICommentTree commentTree)
        {
            var comment = commentTree.Comment;
            await _gitClientService.DeletePullRequestComment(PullRequestId, comment.Id, comment.Version);
            await UpdateComments(PullRequestId); //todo temp solution just to make it work
        }

        public void InitializeCommands()
        {
            ReplyCommentCommand = ReactiveCommand.CreateFromTask<ICommentTree>(ReplyToComment);
            EditCommentCommand = ReactiveCommand.CreateFromTask<ICommentTree>(EditComment);
            DeleteCommentCommand = ReactiveCommand.CreateFromTask<ICommentTree>(DeleteComment);
            AddCommentCommand = ReactiveCommand.CreateFromTask<GitCommentInline>(AddComment);
        }


        public string ErrorMessage { get; set; }
        public IEnumerable<ReactiveCommand> ThrowableCommands => new[] { ReplyCommentCommand, EditCommentCommand, DeleteCommentCommand, AddCommentCommand };
    }
}